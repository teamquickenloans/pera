﻿/**
* FileUpload controller
* @namespace pera.fileupload.controllers
*/
(function () {
    'use strict';

    angular
      .module('pera.fileupload.controllers')
      .controller('FileUploadController', FileUploadController);

    FileUploadController.$inject = ['$scope', '$upload', 'Upload', 'Garages', 'Snackbar', '$filter']; //Here 'Garages' is the Garages Service (pera.garages.service)

    /**
    * @namespace FileUploadController
    */
    function FileUploadController($scope, $upload, Upload, Garages, Snackbar, $filter) {
  
        var vm = this;
        vm.files = [];
        vm.counter = 0;
        vm.reports = [{ garageID: 0 }];

        $scope.garages = [];
        $scope.data = 'none'; //the file

        vm.addReport = addReport;
        $scope.uploadAll = uploadAll;

        //$scope.file = [];

        $scope.invoice = {
            invoiceID: '',
            totalAmountBilled: '',
            dateReceived: '',
            dateUploaded: '',
            monthYear: '',
            totalLeasedSpots: '',
            validations: '',
            reports: vm.reports
        }

        Garages.all().then(garagesSuccessFn, garagesErrorFn);

        function addReport() {
            vm.counter++;
            console.log("add file");
            vm.reports.push({ garageID: 0 });
        }

        function uploadAll()
        {
            var date = Date.now();
            $scope.invoice.dateUploaded = $filter('date')(date, 'MM-dd-yyyy') 
            console.log("Upload all");
            console.log($scope.invoice.dateReceived);

            if (vm.reports && vm.reports.length)
            {
                for (var i = 0; i < vm.reports.length; i++) {
                    console.log(vm.reports[i].file[0].name)
                    vm.files.push(vm.reports[i].file[0])
                }
                Upload.upload(vm.files, $scope.invoice);
            }

        }

        function garagesSuccessFn(data, status, headers, config) {
            $scope.garages = data.data;         //this will depend on what the API returns, it may have to change
            //console.log("fileupload Contoller garages success", $scope.garages);
        }

        function garagesErrorFn(data, status, headers, config) {
            Snackbar.error(data.data.error);
        }

        /**
        * @name upload
        * @desc Try to upload a file using ng-file-upload
        *//*
        function upload(files) {
            console.log($scope.invoice.invoiceID);
            if (files && files.length) {
                for (var i = 0; i < files.length; i++) {
                    var file = files[i];
                    console.log(file.name);
                    (function (index) {
                        console.log("posting..");
                        $scope.uploads[index] = $upload.upload({
                            url: "./api/files/upload", // webapi url
                            method: "POST",
                            data: { invoice: $scope.invoice },
                            file: file
                        }, uploadProgressFn, uploadSuccessFn)
                    })(i);
                }
            }
        };

        function uploadProgressFn(evt) {
            var progressPercentage = parseInt(100.0 * evt.loaded / evt.total);
            console.log('progress: ' + progressPercentage + '% ' + evt.config.file.name);
        };
        function uploadSuccessFn(data, status, headers, config) {
            console.log("posted!");
            console.log('file ' + config.file.name + 'uploaded. Response: ' + JSON.stringify(data));
        };

        function abortUpload(index) {
            $scope.uploads[index].abort();
        }
        
        */
    }
})();
